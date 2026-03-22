(pageNumber) => {
    window.scrollTo(0, pageNumber * 1200);
    return JSON.stringify({ HasNextPage: true });
}
