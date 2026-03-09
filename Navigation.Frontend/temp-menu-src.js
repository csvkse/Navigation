$(document).ready(function () {
    const setupContent = $('#setup-content');
    const menuContent = $('#menu-content');
    const loading = $('#loading');
    const currentMenuNameInput = $('#current-menu-name-input');
    const passwordInput = $('#password');
    const notesList = $('#notes-list');
    const noteContentViewer = $('#note-content-viewer');
    const noteTitle = $('#note-title');
    const noteBody = $('#note-body');

    // Buttons
    const addNoteBtn = $('#add-note-btn');
    const loadMenuBtn = $('#load-menu');
    const addPasswordBtn = $('#add-password-btn');
    const changePasswordBtn = $('#change-password-btn');
    const removePasswordBtn = $('#remove-password-btn');
    const saveNoteBtn = $('#save-note-btn');

    let menuId = null;
    let menuName = null;
    let currentNotes = [];

    function setControlsDisabled(disabled) {
        addNoteBtn.prop('disabled', disabled);
        // Password buttons are handled by updatePasswordButtons
    }

    /**
     * Requirement 4 & 5: Dynamically show/hide password management buttons.
     * @param {boolean} hasPassword - Whether the current menu has a password.
     */
    function updatePasswordButtons(hasPassword) {
        if (hasPassword) {
            addPasswordBtn.hide().prop('disabled', true);
            changePasswordBtn.show().prop('disabled', false);
            removePasswordBtn.show().prop('disabled', false);
        } else {
            addPasswordBtn.show().prop('disabled', false);
            changePasswordBtn.hide().prop('disabled', true);
            removePasswordBtn.hide().prop('disabled', true);
        }
    }

    // --- Modal Handling ---
    function openModal(modalId) {
        $('#' + modalId).show();
    }

    function closeModal(modalId) {
        $('#' + modalId).hide();
        $('#' + modalId + ' input').val('');
    }

    $('.modal .close-btn').on('click', function () {
        closeModal($(this).data('modal'));
    });

    $(window).on('click', function (event) {
        if ($(event.target).is('.modal')) {
            closeModal($(event.target).attr('id'));
        }
    });

    addPasswordBtn.on('click', () => openModal('add-password-modal'));
    changePasswordBtn.on('click', () => openModal('change-password-modal'));
    removePasswordBtn.on('click', () => openModal('remove-password-modal'));

    // --- Note Modal ---
    addNoteBtn.on('click', function () {
        $('#note-modal-title').text('新增笔记');
        $('#original-alias-input').val('');
        $('#note-name-input').val('');
        $('#note-alias-input').val('');
        openModal('note-modal');
    });

    notesList.on('click', '.edit-note-btn', function (e) {
        e.stopPropagation();
        const alias = $(this).closest('.note-item').data('id');
        const note = currentNotes.find(n => n.alias === alias.toString());
        if (note) {
            $('#note-modal-title').text('编辑笔记');
            $('#original-alias-input').val(note.alias);
            $('#note-name-input').val(note.noteName);
            $('#note-alias-input').val(note.alias);
            openModal('note-modal');
        }
    });

    // --- API Calls ---
    function apiRequest(endpoint, method, data, successMsg, errorMsg, successCallback, errorCallback) {
        const url = `${API_BASE_URL}/TempMenu${endpoint}`;
        console.log(`Requesting ${method} ${url} with data:`, data);

        $.ajax({
            url: url,
            type: method,
            contentType: 'application/json',
            data: data ? JSON.stringify(data) : null,
            success: function (response) {
                if (response.success === false) {
                    const message = response.message || '操作失败';
                    alert(`${errorMsg}: ${message}`);
                    if (errorCallback) errorCallback(response);
                    return;
                }
                if (successMsg) alert(successMsg);
                console.log('Success:', response);
                if (successCallback) {
                    successCallback(response);
                }
            },
            error: function (xhr) {
                const error = xhr.responseJSON || { message: xhr.responseText };
                alert(`${errorMsg}: ${error.message || 'Unknown error'}`);
                console.error('Error:', xhr);
                if (errorCallback) {
                    errorCallback();
                }
            }
        });
    }
    
    // --- Core Logic (MVVM-like) ---
    function getMenuNameFromUrl() {
        const params = new URLSearchParams(window.location.search);
        return params.get('m');
    }

    function renderNotes() {
        notesList.empty();
        if (!currentNotes || currentNotes.length === 0) {
            notesList.append('<div class="note-item"><span>暂无笔记</span></div>');
            return;
        }
        const pw = passwordInput.val();
        currentNotes.forEach(note => {
            const noteUrl = `${API_NOTE_URL}/${note.noteName}${pw ? '?pw=' + encodeURIComponent(pw) : ''}`;
            const noteItem = $(`
                <div class="note-item" data-id="${note.alias}" data-name="${note.noteName}">
                    <div>
                        <span class="note-name">${note.noteName}</span>
                        <span class="note-alias">(${note.alias})</span>
                    </div>
                    <div class="note-actions">
                        <a href="${noteUrl}" target="_blank" class="btn btn-primary">查看</a>
                        <button class="btn btn-secondary edit-note-btn">编辑</button>
                        <button class="btn btn-danger delete-note-btn">删除</button>
                    </div>
                </div>
            `);
            notesList.append(noteItem);
        });
    }

    function loadMenuData() {
        menuName = currentMenuNameInput.val() || menuName;
        if (!menuName) return;

        loading.show();
        setupContent.hide();
        menuContent.hide();
        noteContentViewer.hide();
        setControlsDisabled(true);
        updatePasswordButtons(false); // Initially hide all password buttons
        $('#status-message').hide(); // Hide status message on new load

        $.ajax({
            url: `${API_BASE_URL}/TempMenu/byName/${encodeURIComponent(menuName)}?password=${encodeURIComponent(passwordInput.val())}`,
            type: 'GET',
            success: function (response) {
                loading.hide();
                
                if (response.success === false) {
                    menuContent.show();
                    setControlsDisabled(true);
                    updatePasswordButtons(false); // No valid data, assume no password state

                    if (response.message && response.message.toLowerCase().includes('password invalid')) {
                        //alert('Password invalid detected. Trying to open modal.'); // Debugging alert
                        $('#password-prompt-title').text(`菜单 "${menuName}" 需要密码`);
                        openModal('password-prompt-modal');
                        notesList.empty().append('<div class="note-item"><span></span></div>');
                        $('#status-message').text('需要密码才能访问此菜单。').show();
                    } else {
                        // Handle other "success: false" errors
                        $('#status-message').text(response.message || '加载失败，未知错误。').show();
                    }
                    return;
                }
                
                const data = response.data;
                menuId = data.id;
                currentMenuNameInput.val(data.name);
                currentNotes = data.notes || [];
                renderNotes();
                menuContent.show();
                setControlsDisabled(false);
                
                if(data.password==null)
                {
                    passwordInput.val("")
                }
                // Requirement 2: This correctly updates the buttons after a successful load
                updatePasswordButtons(data.password!=null);
            },
            error: function (xhr) {
                loading.hide();
                setControlsDisabled(true);
                updatePasswordButtons(false);
                menuContent.show();

                if (xhr.status === 404) {
                    if (confirm(`菜单 "${menuName}" 不存在。要创建它吗？`)) {
                        createMenu();
                    } else {
                        window.location.href = window.location.pathname;
                    }
                } else {
                    const error = xhr.responseJSON || { message: '服务器连接失败或发生未知错误。' };
                    $('#status-message').text(error.message).show();
                }
            }
        });
    }

    function createMenu() {
        apiRequest('', 'POST', { name: menuName, password: passwordInput.val(), notes: [] },
            `菜单 "${menuName}" 创建成功!`, '创建菜单失败',
            (response) => {
                menuId = response.data.id;
                loadMenuData();
            }
        );
    }

    

    function saveAllNotesToServer() {
        if (!menuId) return;

        const payload = { password: passwordInput.val(), notes: currentNotes };

        apiRequest(`/${menuId}/notes`, 'PUT', payload, null, '笔记保存失败', 
            () => { console.log('Notes saved successfully.'); },
            () => { loadMenuData(); } // On error, reload to revert to last known good state
        );
    }

    // --- Event Handlers ---
    loadMenuBtn.on('click', loadMenuData);

    currentMenuNameInput.on('input', function() {
        const newName = $(this).val();
        menuName = newName; // Update global variable
        const newUrl = window.location.pathname + '?m=' + encodeURIComponent(newName);
        window.history.pushState({path: newUrl}, '', newUrl);
    });

    $('#manual-load-btn').on('click', function () {
        const manualName = $('#manual-menu-name').val().trim();
        if (manualName) {
            menuName = manualName;
            window.history.pushState({}, '', window.location.pathname + '?m=' + encodeURIComponent(menuName));
            loadMenuData();
        } else {
            alert('请输入菜单名称。');
        }
    });

    saveNoteBtn.on('click', function () {
        const originalAlias = $('#original-alias-input').val();
        const newNoteName = $('#note-name-input').val().trim();
        let newAlias = $('#note-alias-input').val().trim();

        if(newNoteName &&  !newAlias){
            newAlias = newNoteName;
        }

        if (!newNoteName || !newAlias) {
            alert('笔记名称和别名均不能为空。');
            return;
        }

        const isDuplicate = currentNotes.some(note => note.alias === newAlias && note.alias !== originalAlias);
        if (isDuplicate) {
            alert('别名不能重复！');
            return;
        }

        if (originalAlias) { // Edit
            const noteToEdit = currentNotes.find(n => n.alias === originalAlias);
            if (noteToEdit) {
                noteToEdit.noteName = newNoteName;
                noteToEdit.alias = newAlias;
            }
        } else { // Add
            currentNotes.push({ noteName: newNoteName, alias: newAlias });
        }
        
        renderNotes();
        saveAllNotesToServer();
        closeModal('note-modal');
        showToast('保存成功');
    });

    notesList.on('click', '.delete-note-btn', function (e) {
        e.stopPropagation();
        if (!confirm('确定要删除此笔记吗？')) return;

        const aliasToDelete = $(this).closest('.note-item').data('id');
        currentNotes = currentNotes.filter(n => n.alias !== aliasToDelete.toString());
        
        renderNotes();
        saveAllNotesToServer();
        showToast('删除成功');
    });

    $('#confirm-prompt-password').on('click', function () {
        const userPassword = $('#prompt-password-input').val();
        passwordInput.val(userPassword);
        closeModal('password-prompt-modal');
        loadMenuData(); // Retry with the new password
    });

    $('#prompt-password-input').on('keypress', function(e) {
        if (e.which === 13) { // Enter key pressed
            $('#confirm-prompt-password').click();
        }
    });

    $('#confirm-add-password').on('click', function () {
        const newPassword = $('#add-new-password').val();
        if (!newPassword) {
            alert('新密码不能为空。');
            return;
        }
        apiRequest(`/${menuId}/password`, 'POST', { newPassword }, '密码新增成功！', '密码新增失败', () => {
            closeModal('add-password-modal');
            updatePasswordButtons(true);
            passwordInput.val(newPassword); // Update main password input for subsequent requests
        });
    });

    $('#confirm-change-password').on('click', function () {
        const currentPassword = $('#change-current-password').val();
        const newPassword = $('#change-new-password').val();
        if (!newPassword) {
            alert('新密码不能为空。');
            return;
        }
        apiRequest(`/${menuId}/password`, 'PUT', { currentPassword, newPassword }, '密码修改成功！', '密码修改失败', () => {
            closeModal('change-password-modal');
            passwordInput.val(newPassword); // Update main password input
        });
    });

    $('#confirm-remove-password').on('click', function () {
        const currentPassword = $('#remove-current-password').val();
        if (!currentPassword) {
            alert('当前密码不能为空。');
            return;
        }
        apiRequest(`/${menuId}/password`, 'DELETE', { currentPassword }, '密码删除成功！', '密码删除失败', () => {
            closeModal('remove-password-modal');
            updatePasswordButtons(false);
            passwordInput.val(''); // Clear main password input
        });
    });

    // --- Initialization ---
    menuName = getMenuNameFromUrl();
    if (menuName) {
        loading.show();
        setupContent.hide();
        currentMenuNameInput.val(menuName);
        loadMenuData();
    } else {
        loading.hide();
        setupContent.show();
        setControlsDisabled(true);
        updatePasswordButtons(false);
    }
});