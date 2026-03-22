() => {
    let description = document.querySelector('#jobDescriptionText')?.innerText ?? '';

    let rawSalary = Array.from(document.querySelectorAll('span[class*="js-match-insights-provider"]'))
        .filter(span => span.textContent.includes('$'))[0]?.textContent ?? '';

    let keywords = [];

    return JSON.stringify({ Description: description, Keywords: keywords });
}
