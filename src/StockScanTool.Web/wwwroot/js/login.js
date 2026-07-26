window.startLogin = async function (dotNetHelper) {
    const btn = document.getElementById('login-btn');
    const usernameEl = document.getElementById('login-username');
    const passwordEl = document.getElementById('login-password');
    const errorEl = document.getElementById('login-error');

    btn.addEventListener('click', async function () {
        btn.disabled = true;
        btn.textContent = 'Signing in...';
        errorEl.style.display = 'none';

        try {
            const resp = await fetch('/api-proxy/auth/admin-login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    username: usernameEl.value,
                    password: passwordEl.value
                })
            });

            if (!resp.ok) {
                errorEl.textContent = 'Invalid username or password.';
                errorEl.style.display = 'block';
                btn.disabled = false;
                btn.textContent = 'Sign In';
                return;
            }

            const data = await resp.json();
            if (data.success && data.data && data.data.token) {
                sessionStorage.setItem('authToken', data.data.token);
                window.location.href = '/';
            } else {
                errorEl.textContent = data.error || 'Login failed.';
                errorEl.style.display = 'block';
                btn.disabled = false;
                btn.textContent = 'Sign In';
            }
        } catch (e) {
            errorEl.textContent = 'Could not connect to the API.';
            errorEl.style.display = 'block';
            btn.disabled = false;
            btn.textContent = 'Sign In';
        }
    });
};
