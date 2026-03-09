import CryptoJS from 'crypto-js'
import pako from 'pako'

const PBKDF2_ITERATIONS = 10

async function compress(data: string): Promise<Uint8Array> {
    const uint8Array = new TextEncoder().encode(data)
    return pako.gzip(uint8Array)
}

async function decompress(compressedData: Uint8Array): Promise<string> {
    const decompressed = pako.ungzip(compressedData)
    return new TextDecoder().decode(decompressed)
}

export async function encrypt(plainText: string, key: string, salt: string): Promise<string> {
    if (!plainText || !key || !salt) return plainText

    // 1. Compress
    const compressedData = await compress(plainText)

    // 2. Derive Key and IV using PBKDF2
    const derivedBytes = CryptoJS.PBKDF2(key, salt, {
        keySize: (32 + 16) / 4,
        iterations: PBKDF2_ITERATIONS,
        hasher: CryptoJS.algo.SHA256
    })

    const aesKey = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(0, 32 / 4))
    const iv = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(32 / 4, (32 + 16) / 4))

    // 3. Encrypt
    const wordArray = CryptoJS.lib.WordArray.create(compressedData as any)
    const encrypted = CryptoJS.AES.encrypt(wordArray, aesKey, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    })

    return encrypted.toString()
}

export async function decrypt(encryptedData: string, key: string, salt: string): Promise<string> {
    if (!encryptedData || !key || !salt) return encryptedData

    // 1. Derive Key and IV
    const derivedBytes = CryptoJS.PBKDF2(key, salt, {
        keySize: (32 + 16) / 4,
        iterations: PBKDF2_ITERATIONS,
        hasher: CryptoJS.algo.SHA256
    })

    const aesKey = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(0, 32 / 4))
    const iv = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(32 / 4, (32 + 16) / 4))

    // 2. Decrypt
    const decrypted = CryptoJS.AES.decrypt(encryptedData, aesKey, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    })

    // 3. Convert WordArray to Uint8Array
    const words = decrypted.words
    const sigBytes = decrypted.sigBytes
    const u8Array = new Uint8Array(sigBytes)
    for (let i = 0; i < sigBytes; i++) {
        const word = words[i >>> 2]
        if (word !== undefined) {
            u8Array[i] = (word >>> (24 - (i % 4) * 8)) & 0xff
        }
    }

    // 4. Decompress
    return await decompress(u8Array)
}
