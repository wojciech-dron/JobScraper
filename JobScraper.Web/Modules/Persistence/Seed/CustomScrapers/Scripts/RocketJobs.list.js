() => {
    let jobItems = document.querySelectorAll("a.offer-card");

    let results = Array.from(jobItems).map(a => {
        let mainDiv = a.querySelector('div > div > div');
        if (!mainDiv || mainDiv.childNodes.length < 3) return null;

        let bottomDiv = mainDiv.childNodes[2];
        let infoDiv = bottomDiv.childNodes[0];
        let keywordsDiv = bottomDiv.childNodes[1];

        return {
            Title: mainDiv.querySelector('h3')?.textContent.trim(),
            Url: window.location.origin + a.getAttribute('href'),
            CompanyName: mainDiv.querySelector('div > div > div > div > p')?.textContent.trim(),
            Location: infoDiv.childNodes?.[1]?.textContent.replace('Locations', ' Locations').trim(),
            SalaryToParse: mainDiv.querySelector('h6')?.textContent.trim(),
            OfferKeywords: [...keywordsDiv.childNodes].map(x => x?.textContent.trim()).slice(1)
        };
    });

    return JSON.stringify(results.filter(x => x != null));
}
