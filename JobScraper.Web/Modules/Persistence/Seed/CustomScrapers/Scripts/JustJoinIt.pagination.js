(pageNumber) => {
    window.scrollTo(0, pageNumber * 1800);
    return JSON.stringify({ HasNextPage: true });
}
