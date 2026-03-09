/**
 * 加密/解密工具，支持 Web Crypto API 和 crypto-js 降级方案
 * 添加了 GZIP 压缩/解压功能
 * 自动检测环境，优先使用 Web Crypto API，非 HTTPS 或旧浏览器回退到 crypto-js
 * 自动动态加载 crypto-js（无需手动引入）
 * 支持全局变量控制 PBKDF2 迭代次数，测试中显示耗时和当前方案
 */

// 全局变量：控制 PBKDF2 迭代次数
let PBKDF2_ITERATIONS = 10; // 增加以提高安全性

// 动态加载 pako（GZIP 压缩库）
let pakoLoaded = false;
let pakoLoadingPromise = null;

async function loadPako() {
    if (pakoLoaded) return;
    if (pakoLoadingPromise) return pakoLoadingPromise;

    pakoLoadingPromise = new Promise((resolve, reject) => {
        if (typeof window === "undefined") {
            reject(new Error("pako cannot be loaded in non-browser environment."));
            return;
        }

        const script = document.createElement("script");
        script.src = "https://cdn.jsdelivr.net/npm/pako@2.1.0/dist/pako.min.js";
        script.async = true;
        script.onload = () => {
            pakoLoaded = true;
            pakoLoadingPromise = null;
            resolve();
        };
        script.onerror = () => {
            pakoLoadingPromise = null;
            reject(new Error("Failed to load pako library."));
        };
        document.head.appendChild(script);
    });

    return pakoLoadingPromise;
}

// GZIP 压缩函数
async function compress(data) {
    await loadPako();
    const uint8Array = new TextEncoder().encode(data);
    return pako.gzip(uint8Array);
}

// GZIP 解压函数
async function decompress(compressedData) {
    await loadPako();
    const decompressed = pako.ungzip(compressedData);
    return new TextDecoder().decode(decompressed);
}

// Web Crypto API 实现（高性能，HTTPS 环境）
async function encryptWebCrypto(plainText, key, salt) {
    if (!plainText || !key || !salt) {
        throw new Error("Plaintext, key, and salt cannot be empty.");
    }

    // 1. 压缩数据
    const compressedData = await compress(plainText);

    const encoder = new TextEncoder();
    const keyBytes = encoder.encode(key);
    const saltBytes = encoder.encode(salt);

    // 使用 PBKDF2 生成 48 字节（32 字节密钥 + 16 字节 IV）
    const derivedKey = await deriveKeyAndIvWebCrypto(keyBytes, saltBytes);
    const aesKey = derivedKey.slice(0, 32);
    const iv = derivedKey.slice(32, 48);

    // 导入 AES 密钥
    const cryptoKey = await crypto.subtle.importKey(
        "raw",
        aesKey,
        { name: "AES-CBC" },
        false,
        ["encrypt"]
    );

    // 执行加密
    const encrypted = await crypto.subtle.encrypt(
        { name: "AES-CBC", iv: iv },
        cryptoKey,
        compressedData
    );

    // 返回 Base64 编码的加密数据
    return btoa(String.fromCharCode(...new Uint8Array(encrypted)));
}

async function decryptWebCrypto(encryptedData, key, salt) {
    if (!encryptedData || !key || !salt) {
        throw new Error("Encrypted data, key, and salt cannot be empty.");
    }

    const encoder = new TextEncoder();
    const keyBytes = encoder.encode(key);
    const saltBytes = encoder.encode(salt);

    // 使用 PBKDF2 生成 48 字节（32 字节密钥 + 16 字节 IV）
    const derivedKey = await deriveKeyAndIvWebCrypto(keyBytes, saltBytes);
    const aesKey = derivedKey.slice(0, 32);
    const iv = derivedKey.slice(32, 48);

    // 导入 AES 密钥
    const cryptoKey = await crypto.subtle.importKey(
        "raw",
        aesKey,
        { name: "AES-CBC" },
        false,
        ["decrypt"]
    );

    // 解码 Base64 加密数据
    const cipherBytes = Uint8Array.from(atob(encryptedData), c => c.charCodeAt(0));

    // 执行解密
    const decrypted = await crypto.subtle.decrypt(
        { name: "AES-CBC", iv: iv },
        cryptoKey,
        cipherBytes
    );

    // 2. 解压数据
    const decompressed = await decompress(new Uint8Array(decrypted));
    return decompressed;
}

async function deriveKeyAndIvWebCrypto(key, salt) {
    const keyMaterial = await crypto.subtle.importKey(
        "raw",
        key,
        { name: "PBKDF2" },
        false,
        ["deriveBits"]
    );

    const derivedBytes = await crypto.subtle.deriveBits(
        {
            name: "PBKDF2",
            salt: salt,
            iterations: PBKDF2_ITERATIONS,
            hash: "SHA-256"
        },
        keyMaterial,
        (32 + 16) * 8 // 48 字节 = 384 位
    );

    return new Uint8Array(derivedBytes);
}

// crypto-js 实现（降级方案，非 HTTPS 或旧浏览器）
async function encryptCryptoJs(plainText, key, salt) {
    if (!plainText || !key || !salt) {
        throw new Error("Plaintext, key, and salt cannot be empty.");
    }

    if (typeof CryptoJS === "undefined") {
        throw new Error("CryptoJS library is not loaded.");
    }

    // 1. 压缩数据
    const compressedData = await compress(plainText);

    // 将压缩数据转换为 WordArray
    const wordArray = CryptoJS.lib.WordArray.create(compressedData);

    // 使用 PBKDF2 生成 48 字节（32 字节密钥 + 16 字节 IV）
    const derivedBytes = CryptoJS.PBKDF2(key, salt, {
        keySize: (32 + 16) / 4, // 48 字节（以 4 字节为单位）
        iterations: PBKDF2_ITERATIONS,
        hasher: CryptoJS.algo.SHA256
    });

    const aesKey = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(0, 32 / 4));
    const iv = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(32 / 4, (32 + 16) / 4));

    // 执行加密
    const encrypted = CryptoJS.AES.encrypt(wordArray, aesKey, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    });

    // 返回 Base64 编码的加密数据
    return encrypted.toString();
}

async function decryptCryptoJs(encryptedData, key, salt) {
    if (!encryptedData || !key || !salt) {
        throw new Error("Encrypted data, key, and salt cannot be empty.");
    }

    if (typeof CryptoJS === "undefined") {
        throw new Error("CryptoJS library is not loaded.");
    }

    // 使用 PBKDF2 生成 48 字节（32 字节密钥 + 16 字节 IV）
    const derivedBytes = CryptoJS.PBKDF2(key, salt, {
        keySize: (32 + 16) / 4,
        iterations: PBKDF2_ITERATIONS,
        hasher: CryptoJS.algo.SHA256
    });

    const aesKey = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(0, 32 / 4));
    const iv = CryptoJS.lib.WordArray.create(derivedBytes.words.slice(32 / 4, (32 + 16) / 4));

    // 执行解密
    const decrypted = CryptoJS.AES.decrypt(encryptedData, aesKey, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    });

    // 2. 解压数据
    // 将 WordArray 转换为 Uint8Array
    const words = decrypted.words;
    const sigBytes = decrypted.sigBytes;
    const u8Array = new Uint8Array(sigBytes);
    for (let i = 0; i < sigBytes; i++) {
        u8Array[i] = (words[i >>> 2] >>> (24 - (i % 4) * 8)) & 0xff;
    }

    const decompressed = await decompress(u8Array);
    return decompressed;
}

// 动态加载 crypto-js
let cryptoJsLoaded = false;
let cryptoJsLoadingPromise = null;

async function loadCryptoJs() {
    if (cryptoJsLoaded) return;
    if (cryptoJsLoadingPromise) return cryptoJsLoadingPromise;

    cryptoJsLoadingPromise = new Promise((resolve, reject) => {
        if (typeof window === "undefined") {
            reject(new Error("CryptoJS cannot be loaded in non-browser environment."));
            return;
        }

        const script = document.createElement("script");
        script.src = "https://cdn.jsdelivr.net/npm/crypto-js@4.2.0/crypto-js.min.js";
        script.async = true;
        script.onload = () => {
            cryptoJsLoaded = true;
            cryptoJsLoadingPromise = null;
            resolve();
        };
        script.onerror = () => {
            cryptoJsLoadingPromise = null;
            reject(new Error("Failed to load CryptoJS library."));
        };
        document.head.appendChild(script);
    });

    return cryptoJsLoadingPromise;
}

// 检测 Web Crypto API 可用性
const isWebCryptoAvailable = () => {
    return typeof window !== "undefined" &&
        window.crypto &&
        window.crypto.subtle &&
        window.location.protocol === "https:";
};

// 加密方法（自动选择实现）
async function encrypt(plainText, key, salt) {
    if (isWebCryptoAvailable()) {
        try {
            return await encryptWebCrypto(plainText, key, salt);
        } catch (e) {
            console.warn("Web Crypto API failed, falling back to CryptoJS:", e.message);
        }
    }

    // 非 HTTPS 或 Web Crypto API 失败，加载 crypto-js
    await loadCryptoJs();
    return await encryptCryptoJs(plainText, key, salt);
}

// 解密方法（自动选择实现）
async function decrypt(encryptedData, key, salt) {
    if (isWebCryptoAvailable()) {
        try {
            return await decryptWebCrypto(encryptedData, key, salt);
        } catch (e) {
            console.warn("Web Crypto API failed, falling back to CryptoJS:", e.message);
        }
    }

    // 非 HTTPS 或 Web Crypto API 失败，加载 crypto-js
    await loadCryptoJs();
    return await decryptCryptoJs(encryptedData, key, salt);
}

// 示例 1：测试自动选择（Web Crypto API 或 crypto-js），显示方案和耗时
async function example() {
    try {
        const plainText = "1Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。";
        const key = "MySecureKey123";
        const salt = "MySecureSalt456";
        const scheme = isWebCryptoAvailable() ? "Web Crypto API" : "CryptoJS";

        console.log(`Running example with ${scheme}, PBKDF2 iterations: ${PBKDF2_ITERATIONS}`);

        // 加密耗时
        const encryptStart = performance.now();
        let encryptedData = await encrypt(plainText, key, salt);
        encryptedData = "OV5y1ZUQpuSyMxbYCzKo/BB8y501m9qB8r7T/Y74jTUHd43s5cWJ9rTWvL6J4MsO5TSux1edrwt+B4ACLSvJ04BeDXDj1E0b9yKBJxgRtEMZcKh69jUrOcS3yLtJPLEJk6Oc5U3BrNmbVrC9YYm/Hf373IolHicCbhdTrdwMBZY=";
        const encryptTime = performance.now() - encryptStart;
        console.log(`Encrypted (auto-select): ${encryptedData.length} ${encryptedData}`);
        console.log(`Encryption time: ${encryptTime.toFixed(2)} ms`);

        // 解密耗时
        const decryptStart = performance.now();
        const decryptedText = await decrypt(encryptedData, key, salt);
        const decryptTime = performance.now() - decryptStart;
        console.log(`Decrypted (auto-select): ${decryptedText}`);
        console.log(`Decryption time: ${decryptTime.toFixed(2)} ms`);

        // 验证结果
        if (decryptedText === plainText) {
            console.log(`${scheme} test passed: Decrypted text matches original.`);
        } else {
            console.error(`${scheme} test failed: Decrypted text does not match original.`);
        }
    } catch (error) {
        console.error("Error in example:", error.message);
    }
}

// 示例 2：专门测试 crypto-js 实现，显示耗时
async function example2() {
    try {
        const plainText = "1Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。Hello, World! 这是一段需要加密的敏感数据，添加了GZIP压缩功能。";
        const key = "MySecureKey123";
        const salt = "MySecureSalt456";

        console.log(`Running example2 with CryptoJS, PBKDF2 iterations: ${PBKDF2_ITERATIONS}`);

        // 确保 crypto-js 和 pako 已加载
        await loadCryptoJs();
        await loadPako();

        // 加密耗时
        const encryptStart = performance.now();
        let encryptedData = await encryptCryptoJs(plainText, key, salt);
        encryptedData = "OV5y1ZUQpuSyMxbYCzKo/BB8y501m9qB8r7T/Y74jTUHd43s5cWJ9rTWvL6J4MsO5TSux1edrwt+B4ACLSvJ04BeDXDj1E0b9yKBJxgRtEMZcKh69jUrOcS3yLtJPLEJk6Oc5U3BrNmbVrC9YYm/Hf373IolHicCbhdTrdwMBZY=";
        const encryptTime = performance.now() - encryptStart;
        console.log(`Encrypted (crypto-js): ${encryptedData.length} ${encryptedData}`);
        console.log(`Encryption time: ${encryptTime.toFixed(2)} ms`);

        // 解密耗时
        const decryptStart = performance.now();
        const decryptedText = await decryptCryptoJs(encryptedData, key, salt);
        const decryptTime = performance.now() - decryptStart;
        console.log(`Decrypted (crypto-js): ${decryptedText}`);
        console.log(`Decryption time: ${decryptTime.toFixed(2)} ms`);

        // 验证结果
        if (decryptedText === plainText) {
            console.log("CryptoJS test passed: Decrypted text matches original.");
        } else {
            console.error("CryptoJS test failed: Decrypted text does not match original.");
        }
    } catch (error) {
        console.error("Error in example2:", error.message);
    }
}
