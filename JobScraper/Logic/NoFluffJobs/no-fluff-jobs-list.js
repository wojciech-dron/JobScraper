() => {
    const jobItems = document.querySelectorAll("a.posting-list-item")

    const results = Array.from(jobItems).map(a => {
        const aside = a.querySelector('aside')

        return {
            Title: aside.querySelector('header')?.textContent.trim(),
            Url: a.getAttribute('href'),
            CompanyName: aside.querySelector('footer > h4')?.textContent.trim(),
            Location: aside.querySelector('footer > nfj-posting-item-city > div > span')?.textContent.trim(),
            Salary: aside.querySelector('nfj-posting-item-salary')?.textContent.trim(),
            OfferKeywords: [...aside.querySelectorAll('nfj-posting-item-tiles > span')].map(s => s?.textContent.trim())
        };
    });

    console.log(results)

    return JSON.stringify(results)
};