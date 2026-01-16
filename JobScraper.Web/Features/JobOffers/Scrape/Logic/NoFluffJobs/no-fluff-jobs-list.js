() => {
    let jobItems = document.querySelectorAll("a.posting-list-item")
    // let a = jobItems[0];

    let results = Array.from(jobItems).map(a => {
        let aside = a.querySelector('aside')

        let result = {
            Title: aside.querySelector('h3')?.textContent.trim(),
            Url: a.getAttribute('href'),
            CompanyName: aside.querySelector('footer > h4')?.textContent.trim(),
            Location: aside.querySelector('footer > nfj-posting-item-city > div > span')?.textContent.trim(),
            Salary: aside.querySelector('nfj-posting-item-salary')?.textContent.trim(),
            OfferKeywords: [...aside.querySelectorAll('nfj-posting-item-tiles > span')].map(s => s?.textContent.trim())
        };

        console.log(result)

        return result;
    });

    console.log(results)

    return JSON.stringify(results)
};
