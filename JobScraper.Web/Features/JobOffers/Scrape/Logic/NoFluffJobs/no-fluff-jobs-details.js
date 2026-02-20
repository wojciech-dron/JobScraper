() => {
    // click expand button if any
    document.querySelector('span[data-cy="text-fold"]')?.click()

    let result = {
        Description: document.querySelector('common-posting-content-wrapper')?.textContent.trim(),
        Keywords: [...document.querySelector('section[commonpostingrequirements]').querySelectorAll('li')].map(x => x?.textContent?.trim()),
        CompanyUrl: document.querySelector('#postingCompanyUrl').getAttribute('href')
    }
    console.log(result)

    return JSON.stringify(result)
};
