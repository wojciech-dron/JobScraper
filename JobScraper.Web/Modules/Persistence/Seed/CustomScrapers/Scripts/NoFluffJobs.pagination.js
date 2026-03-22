() => {
    const button = document.querySelector('button[nfjloadmore]');
    if (!button) return JSON.stringify({ HasNextPage: false });
    button.click();
    return JSON.stringify({ HasNextPage: true });
}
