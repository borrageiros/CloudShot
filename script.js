var repoOwner = "borrageiros";
var repoName = "CloudShot";
var apiUrl = "https://api.github.com/repos/" + repoOwner + "/" + repoName + "/releases/latest";

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

document.addEventListener("DOMContentLoaded", initDownloads);
