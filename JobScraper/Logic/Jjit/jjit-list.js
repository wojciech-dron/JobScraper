() => {
    const jobItems = document.querySelectorAll("a.offer-card")

    const results = Array.from(jobItems).map(a => {
        const div = a.querySelector('div')
        const bottomDiv = div.childNodes[2]
        const infoDiv = bottomDiv.childNodes[0]
        const keywordsDiv = bottomDiv.childNodes[1]

        return {
            Title: div.querySelector('h3')?.textContent.trim(),
            Url: a.getAttribute('href'),
            CompanyName: div.querySelector('div > div > div > div > p')?.textContent.trim(),
            Location: infoDiv.childNodes?.[1]?.textContent.replace('Locations', ' Locations').trim(),
            Salary: div.querySelector('h6')?.textContent.trim(),
            OfferKeywords: [...keywordsDiv.childNodes].map(x => x?.textContent.trim()).slice(1)
        };
    });

    console.log(results)

    return JSON.stringify(results)
};
