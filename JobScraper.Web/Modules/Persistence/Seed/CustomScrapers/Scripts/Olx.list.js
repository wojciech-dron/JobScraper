() => {
    let cards = document.querySelectorAll('div.jobs-ad-card');

    let results = Array.from(cards).map(offer => {
        let titleContainer = [...offer.querySelectorAll('div > div > a')].at(-1);
        let firstRow = offer.querySelector('div:nth-child(2) > div > div');
        let secondRow = offer.querySelector('div:nth-child(2) > div > div:nth-child(2)');

        let title = titleContainer?.textContent.trim() ?? '';
        let url = titleContainer?.getAttribute('href') ?? '';
        let companyName = titleContainer?.parentNode?.querySelector('p')?.textContent.trim() ?? '';

        let firstRowData = [...(firstRow?.querySelectorAll('div > div > p') ?? [])]
            .map(p => p.textContent.trim()).reverse();
        let secondRowData = [...(secondRow?.querySelectorAll('button') ?? [])]
            .map(b => b.textContent.trim());

        // First item in reversed firstRowData may be salary, last is location, rest are keywords
        let location = firstRowData.length > 0 ? firstRowData[firstRowData.length - 1] : '';
        let keywords = [...firstRowData.slice(0, -1), ...secondRowData];

        return {
            Title: title,
            Url: url,
            CompanyName: companyName,
            Location: location,
            OfferKeywords: keywords
        };
    });

    return JSON.stringify(results);
}
