() => {
    document.querySelector('span[data-cy="text-fold"]')?.click();

    let result = {
        Description: document.querySelector('common-posting-content-wrapper')?.textContent.trim(),
        Keywords: [...(document.querySelector('section[commonpostingrequirements]')?.querySelectorAll('li') ?? [])].map(x => x?.textContent?.trim())
    };

    return JSON.stringify(result);
}
