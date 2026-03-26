// Theme manager for dark mode persistence
window.themeManager = {
    get: function () {
        return localStorage.getItem('theme') || 'light';
    },
    set: function (theme) {
        localStorage.setItem('theme', theme);
        document.documentElement.classList.toggle('dark', theme === 'dark');
    }
};
