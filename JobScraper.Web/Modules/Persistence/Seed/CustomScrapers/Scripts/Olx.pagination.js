() => {
    const nextBtn = document.querySelector("a[data-testid='pagination-forward']");
    if (!nextBtn) return JSON.stringify({ HasNextPage: false });
    nextBtn.click();
    return JSON.stringify({ HasNextPage: true });
}
