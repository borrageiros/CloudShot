var repoOwner = "borrageiros";
var repoName = "CloudShot";
var apiUrl = "https://api.github.com/repos/" + repoOwner + "/" + repoName + "/releases/latest";
var readmeUrl = "https://raw.githubusercontent.com/" + repoOwner + "/" + repoName + "/main/README.md";
var readmeSourceUrl = "https://github.com/" + repoOwner + "/" + repoName + "/blob/main/README.md";

function selectAsset(assets, matcher) {
  for (var i = 0; i < assets.length; i++) {
    if (matcher(assets[i])) {
      return assets[i];
    }
  }
  return null;
}

function isPortable(asset) {
  var name = asset.name.toLowerCase();
  return name.indexOf("portable") !== -1 && name.indexOf(".zip") !== -1;
}

function isInstaller(asset) {
  var name = asset.name.toLowerCase();
  if (name.indexOf("installer") !== -1) {
    return true;
  }
  if (name.indexOf("setup") !== -1) {
    return true;
  }
  return false;
}

function updateLink(elementId, asset, defaultHref, label) {
  var el = document.getElementById(elementId);
  if (!el) {
    return;
  }
  if (asset && asset.browser_download_url) {
    el.href = asset.browser_download_url;
    el.textContent = label;
    el.classList.remove("disabled");
  } else {
    el.href = defaultHref;
    el.textContent = label + " (GitHub latest release page)";
  }
}

function isHeading(line) {
  return /^#{1,6}\s/.test(line);
}

function isDownloadHeading(line) {
  return isHeading(line) && /download/i.test(line);
}

function stripReadmeForPage(markdown) {
  var lines = markdown.replace(/\r\n/g, "\n").split("\n");
  var output = [];
  var skipDownloadSection = false;
  var skippedTitle = false;

  for (var i = 0; i < lines.length; i++) {
    var line = lines[i];

    if (!skippedTitle && /^#\s/.test(line)) {
      skippedTitle = true;
      continue;
    }

    if (isDownloadHeading(line)) {
      skipDownloadSection = true;
      continue;
    }

    if (skipDownloadSection) {
      if (isHeading(line)) {
        skipDownloadSection = false;
        output.push(line);
      }
      continue;
    }

    output.push(line);
  }

  return output.join("\n").trim();
}

function enhanceReadmeLinks(container) {
  var links = container.querySelectorAll("a[href^='http']");
  for (var i = 0; i < links.length; i++) {
    links[i].target = "_blank";
    links[i].rel = "noopener noreferrer";
  }
}

function initReadme() {
  var container = document.getElementById("readme-content");
  if (!container) {
    return;
  }

  fetch(readmeUrl)
    .then(function (response) {
      if (!response.ok) {
        throw new Error("Failed to fetch README");
      }
      return response.text();
    })
    .then(function (markdown) {
      var trimmed = stripReadmeForPage(markdown);
      container.innerHTML = marked.parse(trimmed);
      container.classList.remove("loading");
      enhanceReadmeLinks(container);
    })
    .catch(function () {
      container.classList.remove("loading");
      container.innerHTML =
        '<p class="note">Could not load README automatically. ' +
        '<a href="' + readmeSourceUrl + '" target="_blank" rel="noopener noreferrer">View on GitHub</a>.</p>';
    });
}

function initDownloads() {
  var latestVersionLabel = document.getElementById("latest-version-label");
  var latestButton = document.getElementById("btn-latest");
  var note = document.getElementById("download-note");

  fetch(apiUrl)
    .then(function (response) {
      if (!response.ok) {
        throw new Error("Failed to fetch latest release");
      }
      return response.json();
    })
    .then(function (data) {
      var tag = data.tag_name || "";
      var version = tag.replace(/^v/i, "") || tag;
      var assets = Array.isArray(data.assets) ? data.assets : [];

      if (latestVersionLabel && version) {
        latestVersionLabel.textContent = "Latest version: v" + version;
      }

      if (latestButton && assets.length > 0) {
        var installer = selectAsset(assets, isInstaller);
        if (installer && installer.browser_download_url) {
          latestButton.href = installer.browser_download_url;
          latestButton.textContent = "Download v" + version;
        }
      }

      var portable = selectAsset(assets, isPortable);
      var installerAsset = selectAsset(assets, isInstaller);
      var latestPage = "https://github.com/" + repoOwner + "/" + repoName + "/releases/latest";

      updateLink("btn-installer", installerAsset, latestPage, "Installer");
      updateLink("btn-portable", portable, latestPage, "Portable ZIP");

      if (note && assets.length > 0) {
        note.textContent = "Download links point to files from the latest GitHub release.";
      }
    })
    .catch(function () {
      if (latestVersionLabel) {
        latestVersionLabel.textContent = "Latest version information is temporarily unavailable.";
      }
      if (note) {
        note.textContent = "Could not resolve download links automatically. Please use the GitHub releases page.";
      }
    });
}

document.addEventListener("DOMContentLoaded", function () {
  initDownloads();
  initReadme();
});
