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
