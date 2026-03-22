() => {
    let titleElements = document.querySelectorAll('h2.jobTitle');
    let companyElements = document.querySelectorAll("[data-testid='company-name']");
    let locationElements = document.querySelectorAll("[data-testid='text-location']");

    let results = Array.from(titleElements).map((titleEl, i) => {
        let anchor = titleEl.querySelector('a');
        let rawUrl = anchor?.getAttribute('href') ?? '';
        let url = window.location.origin + rawUrl.split('&')[0].replace('/rc/clk', '/viewjob');

        return {
            Title: titleEl.innerText.trim(),
            Url: url,
            CompanyName: companyElements[i]?.innerText.trim() ?? '',
            Location: locationElements[i]?.innerText.trim() ?? '',
            OfferKeywords: []
        };
    });

    return JSON.stringify(results);
}
