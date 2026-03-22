() => {
    // Expand hidden descriptions
    document.querySelectorAll('[data-test="section-short-description"] div.invisible span')
        .forEach(el => el.click());

    let offers = document.querySelectorAll('div[data-test="positioned-offer"], div[data-test="default-offer"]');

    let results = Array.from(offers).map(offer => {
        let title = offer.querySelector('h2[data-test="offer-title"]')?.textContent.trim() ?? '';
        let links = [...offer.querySelectorAll('a[data-test="link-offer"]')].map(a => a.getAttribute('href'));
        let url = links[0]?.split('?')[0] ?? '';
        let salary = offer.querySelector('span[data-test="offer-salary"]')?.textContent.trim() ?? '';
        let jobKeys = [...offer.querySelectorAll('ul > li')].map(li => li.textContent.trim());
        let description = offer.querySelector('[data-test="section-short-description"]')?.textContent.trim() ?? '';
        let companyName = offer.querySelector('[data-test="text-company-name"]')?.textContent.trim() ?? '';
        let location = offer.querySelector('h4[data-test="text-region"]')?.textContent.trim() ?? '';

        return {
            Title: title,
            Url: url,
            CompanyName: companyName,
            Location: location,
            SalaryToParse: salary,
            OfferKeywords: jobKeys,
            Description: description
        };
    });

    return JSON.stringify(results);
}
