const fs = require("fs");
const path = require("path");

const candidates = [
  path.resolve(__dirname, "../publish/wwwroot"),
  path.resolve(__dirname, "../SecretLetter.Browser/bin/Release/net8.0/browser-wasm/publish/wwwroot"),
];
const src = candidates.find((p) => fs.existsSync(p));
const dest = path.resolve(__dirname, "dist");

function copyRecursive(source, target) {
  if (!fs.existsSync(target)) fs.mkdirSync(target, { recursive: true });
  for (const entry of fs.readdirSync(source, { withFileTypes: true })) {
    const srcPath = path.join(source, entry.name);
    const destPath = path.join(target, entry.name);
    if (entry.isDirectory()) {
      copyRecursive(srcPath, destPath);
    } else {
      fs.copyFileSync(srcPath, destPath);
    }
  }
}

if (!fs.existsSync(src)) {
  console.error(
    "Published wwwroot not found. Run dotnet publish first:\n" +
      "  dotnet publish SecretLetter.Browser/SecretLetter.Browser.csproj -c Release"
  );
  process.exit(1);
}

copyRecursive(src, dest);
console.log(`Copied ${src} -> ${dest}`);
