() => {
    let lists = document.querySelectorAll('nfj-postings-list[listname="search"]');
    let jobItems = Array.from(lists).flatMap(list =>
        Array.from(list.querySelectorAll("a.posting-list-item"))
    );

    let results = Array.from(jobItems).map(a => {
        let aside = a.querySelector('aside');
        return {
            Title: aside.querySelector('h3')?.textContent.trim(),
            Url: window.location.origin + a.getAttribute('href'),
            CompanyName: aside.querySelector('footer > h4')?.textContent.trim(),
            Location: aside.querySelector('footer > nfj-posting-item-city > div > span')?.textContent.trim(),
            SalaryToParse: aside.querySelector('nfj-posting-item-salary')?.textContent.trim(),
            OfferKeywords: [...aside.querySelectorAll('nfj-posting-item-tiles > span')].map(s => s?.textContent.trim())
        };
    });

    return JSON.stringify(results);
}
