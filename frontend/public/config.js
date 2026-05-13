window.__ISSUEFORGE_API_BASE_URL__ =
  window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1"
    ? "http://localhost:5008/api"
    : `${window.location.origin}/api`;
