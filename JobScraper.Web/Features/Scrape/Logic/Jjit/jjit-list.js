() => {
    let jobItems = document.querySelectorAll("a.offer-card")
    // let a = jobItems[0]

    let results = Array.from(jobItems).map(a => {
        let mainDiv = a.querySelector('div').querySelector('div').querySelector('div')

        if (mainDiv.childNodes.length < 3)
            return null;

        let bottomDiv = mainDiv.childNodes[2]
        let infoDiv = bottomDiv.childNodes[0]
        let keywordsDiv = bottomDiv.childNodes[1]

        let result = {
            Title: mainDiv.querySelector('h3')?.textContent.trim(),
            Url: a.getAttribute('href'),
            CompanyName: mainDiv.querySelector('div > div > div > div > p')?.textContent.trim(),
            Location: infoDiv.childNodes?.[1]?.textContent.replace('Locations', ' Locations').trim(),
            Salary: mainDiv.querySelector('h6')?.textContent.trim(),
            OfferKeywords: [...keywordsDiv.childNodes].map(x => x?.textContent.trim()).slice(1)
        };

        console.log(result)
        return result;
    });

    console.log(results)

    return JSON.stringify(results.filter(x => x != null))
};
