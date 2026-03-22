() => {
    const nextBtn = document.querySelector("button[data-test='bottom-pagination-button-next']");
    if (!nextBtn) return JSON.stringify({ HasNextPage: false });
    nextBtn.click();
    return JSON.stringify({ HasNextPage: true });
}
