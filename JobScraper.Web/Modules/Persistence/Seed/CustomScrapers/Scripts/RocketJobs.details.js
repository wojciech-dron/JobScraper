() => {
    let descDiv = document.querySelector('h3')?.parentNode;
    if (!descDiv) return JSON.stringify({ Description: '', Keywords: [] });

    const result = {
        Description: [...descDiv.childNodes].slice(1).map(x => x?.textContent).join('\n'),
        Keywords: [...descDiv.querySelectorAll('h4')].map(x => x?.textContent?.trim())
    };

    return JSON.stringify(result);
}
