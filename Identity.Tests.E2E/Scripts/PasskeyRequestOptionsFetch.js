async ([route, username, tokenName, tokenValue]) => {
    const response = await fetch(
        `${route}?username=${encodeURIComponent(username)}`,
        { method: 'POST', credentials: 'include', headers: { [tokenName]: tokenValue } });
    const body = response.ok ? await response.text() : '';
    return JSON.stringify({ status: response.status, body });
}
