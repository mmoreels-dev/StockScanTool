function getApiBaseUrl() {
    var meta = document.querySelector('meta[name="api-base-url"]');
    return meta ? meta.getAttribute('content') : 'http://localhost:5168';
}

function decodeToken(token) {
    try {
        var payload = JSON.parse(atob(token.split('.')[1]));
        return {
            displayName: payload.displayName || payload.sub || 'User',
            roles: payload.role ? (Array.isArray(payload.role) ? payload.role : [payload.role]) : [],
            permissions: payload.permission ? (Array.isArray(payload.permission) ? payload.permission : [payload.permission]) : []
        };
    } catch (e) {
        return null;
    }
}

window.authLogin = async function (username, password) {
    var apiBase = getApiBaseUrl();
    try {
        var resp = await fetch(apiBase + '/api/v1/Auth/admin-login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username: username, password: password })
        });
        var body = await resp.json();
        if (body.success && body.data && body.data.token) {
            localStorage.setItem('auth_token', body.data.token);
            var info = decodeToken(body.data.token);
            if (info) {
                localStorage.setItem('auth_displayName', info.displayName);
                localStorage.setItem('auth_roles', JSON.stringify(info.roles));
                localStorage.setItem('auth_permissions', JSON.stringify(info.permissions));
            }
            return { success: true };
        }
        return { success: false, error: body.error || 'Invalid credentials.' };
    } catch (e) {
        return { success: false, error: 'Could not connect to the API. Make sure the server is running on ' + apiBase };
    }
};

window.authGetToken = function () {
    return localStorage.getItem('auth_token');
};

window.authGetUserInfo = function () {
    return {
        displayName: localStorage.getItem('auth_displayName') || 'User',
        roles: JSON.parse(localStorage.getItem('auth_roles') || '[]'),
        permissions: JSON.parse(localStorage.getItem('auth_permissions') || '[]')
    };
};

window.authLogout = function () {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_displayName');
    localStorage.removeItem('auth_roles');
    localStorage.removeItem('auth_permissions');
};
