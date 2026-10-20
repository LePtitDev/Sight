// Generates the HTML and PDF versions of the specifications.
//
// Usage (from this folder):
//   npm install
//   npm run build                  → every docs/specs/*.md
//   node build.mjs ../sbdl-text.md → selected files only
//
// PDF output is printed by a Chromium-based browser (Edge or Chrome), found automatically
// or given by the SPEC_BROWSER environment variable. Use --no-pdf to only produce HTML.
import { execFileSync } from "node:child_process";
import { existsSync, mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { marked } from "marked";

const specsDir = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const args = process.argv.slice(2);
const withPdf = !args.includes("--no-pdf");
const inputs = args.filter((a) => !a.startsWith("--")).map((a) => resolve(a));
const sources = inputs.length > 0
  ? inputs
  : readdirSync(specsDir).filter((f) => f.endsWith(".md")).map((f) => join(specsDir, f));

function findBrowser() {
  const candidates = [
    process.env.SPEC_BROWSER,
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    "/usr/bin/microsoft-edge",
    "/usr/bin/google-chrome",
    "/usr/bin/chromium",
  ];
  return candidates.find((c) => c && existsSync(c));
}

function printPdf(browser, htmlPath) {
  const pdfPath = htmlPath.replace(/\.html$/, ".pdf");
  const profile = mkdtempSync(join(tmpdir(), "spec-pdf-"));
  try {
    execFileSync(browser, [
      "--headless=new", "--disable-gpu", "--no-first-run", `--user-data-dir=${profile}`,
      "--no-pdf-header-footer", `--print-to-pdf=${pdfPath}`, pathToFileURL(htmlPath).href,
    ], { stdio: "ignore" });
  } finally {
    rmSync(profile, { recursive: true, force: true });
  }
  console.log("written", pdfPath);
}

const slug = (text) =>
  text.toLowerCase().replace(/<[^>]+>/g, "").replace(/[^\w\s.-]/g, "").trim().replace(/[\s.]+/g, "-");

const stripTags = (html) => html.replace(/<[^>]+>/g, "");

// Wraps RFC 2119 keywords found in text nodes (outside <pre>/<code>) in a styled span.
function markKeywords(html) {
  let depth = 0;
  return html.split(/(<[^>]+>)/).map((part) => {
    if (part.startsWith("<")) {
      if (/^<(pre|code)[\s>]/.test(part)) depth++;
      else if (/^<\/(pre|code)>/.test(part)) depth--;
      return part;
    }
    return depth > 0 ? part : part.replace(/\b(MUST NOT|MUST|SHOULD NOT|SHOULD|MAY)\b/g, '<span class="kw">$1</span>');
  }).join("");
}

const browser = withPdf ? findBrowser() : undefined;
if (withPdf && !browser) {
  console.error("No Chromium-based browser found: set SPEC_BROWSER or use --no-pdf.");
  process.exit(1);
}

for (const path of sources) {
  const md = readFileSync(path, "utf8");
  const toc = [];
  const renderer = new marked.Renderer();
  renderer.heading = (text, level) => {
    const id = slug(stripTags(text));
    const numbered = text.replace(/^((?:\d+\.)*\d+\.?|Appendix [A-Z]\.)\s+/, '<span class="num">$1</span> ');
    if (level === 2 || level === 3) toc.push({ level, html: numbered, id });
    if (level === 1) return `<h1>${text}</h1>\n`;
    return `<h${level} id="${id}">${numbered}<a class="anchor" href="#${id}" aria-label="Link to this section">#</a></h${level}>\n`;
  };
  renderer.link = (href, title, text) =>
    `<a href="${href.replace(/\.md(#|$)/, ".html$1")}"${title ? ` title="${title}"` : ""}>${text}</a>`;

  let html = marked.parse(md, { renderer, gfm: true })
    .replace(/<thead>\s*<tr>\s*(<th>\s*<\/th>\s*)+<\/tr>\s*<\/thead>/g, "");
  html = markKeywords(html).replace(/<strong><span class="kw">([^<]+)<\/span><\/strong>/g, '<span class="kw">$1</span>');

  // Split "title + metadata table + <hr>" from the rest of the document.
  const head = html.match(/^\s*<h1>([\s\S]*?)<\/h1>\s*<table>([\s\S]*?)<\/table>\s*<hr>/);
  const title = stripTags(head[1]);
  const meta = [...head[2].matchAll(/<tr>\s*<td>([\s\S]*?)<\/td>\s*<td>([\s\S]*?)<\/td>\s*<\/tr>/g)]
    .map(([, k, v]) => `<div><dt>${stripTags(k)}</dt><dd>${v}</dd></div>`).join("");
  const article = html.slice(head[0].length);

  const tocHtml = toc.map((t) =>
    `<li class="l${t.level}"><a href="#${t.id}">${t.html.replace(/<span class="kw">([^<]+)<\/span>/g, "$1")}</a></li>`).join("");

  const page = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${title}</title>
<style>
:root {
  --ink: #1b1e27; --ink-2: #4a5163; --ink-3: #7c8396;
  --paper: #ffffff; --paper-2: #f6f7fa; --paper-3: #eef0f5;
  --line: #e2e5ec; --accent: #3d5afe; --accent-ink: #2a3fc4; --accent-soft: #eef1ff;
  --kw: #b4235a;
  --sans: "Segoe UI Variable Text", "Segoe UI", system-ui, -apple-system, "Helvetica Neue", sans-serif;
  --display: "Segoe UI Variable Display", "Segoe UI", system-ui, -apple-system, sans-serif;
  --mono: "Cascadia Mono", "Cascadia Code", Consolas, "SF Mono", Menlo, monospace;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    --ink: #e6e8ef; --ink-2: #b1b7c6; --ink-3: #848b9c;
    --paper: #13151b; --paper-2: #1a1d25; --paper-3: #232733;
    --line: #2c303c; --accent: #8c9eff; --accent-ink: #aab8ff; --accent-soft: #1f2540;
    --kw: #ff8fb4;
  }
}
* { box-sizing: border-box; }
html { -webkit-text-size-adjust: 100%; scroll-padding-top: 24px; }
body { margin: 0; background: var(--paper); color: var(--ink); font: 15.5px/1.65 var(--sans); }

/* Title block */
.masthead { border-bottom: 1px solid var(--line); background:
  linear-gradient(180deg, var(--accent-soft), var(--paper) 85%); }
.masthead-inner { max-width: 1180px; margin: 0 auto; padding: 48px 16px 32px; }
.eyebrow { font: 600 12px/1 var(--sans); letter-spacing: .14em; text-transform: uppercase; color: var(--accent-ink); margin: 0 0 14px; }
h1 { font: 700 clamp(28px, 4.5vw, 40px)/1.15 var(--display); letter-spacing: -.02em; margin: 0 0 24px; }
.meta { display: flex; flex-wrap: wrap; gap: 10px 36px; margin: 0; }
.meta div { min-width: 0; }
.meta dt { font-size: 11.5px; letter-spacing: .08em; text-transform: uppercase; color: var(--ink-3); }
.meta dd { margin: 2px 0 0; font-weight: 500; }
.meta code { background: none; padding: 0; }

/* Layout */
.layout { max-width: 1180px; margin: 0 auto; padding: 0 16px 80px; display: grid; grid-template-columns: 250px minmax(0, 1fr); gap: 56px; }
.toc { position: sticky; top: 0; align-self: start; max-height: 100vh; overflow-y: auto; padding: 32px 0 32px; font-size: 13.5px; }
.toc-title { font: 600 11.5px/1 var(--sans); letter-spacing: .1em; text-transform: uppercase; color: var(--ink-3); margin: 0 0 12px; }
.toc ul { list-style: none; margin: 0; padding: 0; }
.toc li a { display: block; padding: 3px 10px; color: var(--ink-2); border-left: 2px solid var(--line); text-decoration: none; }
.toc li.l2 a { font-weight: 500; color: var(--ink); margin-top: 6px; }
.toc li.l3 a { padding-left: 22px; }
.toc li a:hover { color: var(--accent-ink); border-left-color: var(--accent); }
.toc li a.active { color: var(--accent-ink); border-left-color: var(--accent); background: var(--accent-soft); }
.toc .num { color: var(--ink-3); font-variant-numeric: tabular-nums; margin-right: 2px; }
.toc code { background: none; padding: 0; font-size: .95em; }
article { min-width: 0; max-width: 820px; padding-top: 8px; }

/* Headings */
h2, h3, h4 { font-family: var(--display); letter-spacing: -.01em; position: relative; }
h2 { font-size: 26px; line-height: 1.25; margin: 56px 0 16px; padding-top: 12px; }
h3 { font-size: 19px; line-height: 1.3; margin: 36px 0 10px; }
h2 .num, h3 .num { color: var(--accent); font-variant-numeric: tabular-nums; margin-right: 4px; }
.anchor { margin-left: 8px; color: var(--ink-3); opacity: 0; text-decoration: none; font-weight: 400; }
h2:hover .anchor, h3:hover .anchor { opacity: 1; }
hr { border: 0; height: 1px; background: var(--line); margin: 40px 0 0; }

/* Text */
p, ul, ol { margin: 0 0 14px; }
li { margin: 3px 0; }
li > ul, li > ol { margin: 4px 0; }
a { color: var(--accent-ink); text-decoration: underline; text-decoration-color: color-mix(in srgb, var(--accent) 35%, transparent); text-underline-offset: 2px; }
a:hover { text-decoration-color: var(--accent); }
strong { font-weight: 600; }
.kw { font-size: .82em; font-weight: 700; letter-spacing: .04em; color: var(--kw); }
blockquote { margin: 16px 0; padding: 10px 16px; border-left: 3px solid var(--accent); background: var(--accent-soft); border-radius: 0 8px 8px 0; color: var(--ink-2); }
blockquote p:last-child { margin: 0; }

/* Code */
code { font-family: var(--mono); font-size: .86em; background: var(--paper-3); padding: .12em .38em; border-radius: 5px; }
pre { background: var(--paper-2); border: 1px solid var(--line); border-radius: 10px; padding: 14px 18px; overflow-x: auto; margin: 0 0 18px; line-height: 1.5; }
pre code { background: none; padding: 0; font-size: 13px; }

/* Tables */
.table-wrap { overflow-x: auto; margin: 0 0 20px; border: 1px solid var(--line); border-radius: 10px; }
table { border-collapse: collapse; width: 100%; font-size: 14px; }
th, td { padding: 8px 14px; text-align: left; vertical-align: top; border-bottom: 1px solid var(--line); }
tr:last-child td { border-bottom: 0; }
th { background: var(--paper-2); font-size: 12px; font-weight: 600; letter-spacing: .05em; text-transform: uppercase; color: var(--ink-2); }
tbody tr:nth-child(even) td { background: color-mix(in srgb, var(--paper-2) 55%, transparent); }
td code { white-space: nowrap; }

@media (max-width: 900px) {
  .layout { grid-template-columns: minmax(0, 1fr); gap: 0; }
  .toc { position: static; max-height: none; padding: 24px 0 8px; border-bottom: 1px solid var(--line); }
  td code { white-space: normal; }
}

/* Print / PDF */
@page { size: A4; margin: 20mm 17mm 18mm;
  @bottom-left { content: "${title.replace(/"/g, '\\"')}"; font: 8.5pt "Segoe UI", sans-serif; color: #7c8396; }
  @bottom-right { content: counter(page) " / " counter(pages); font: 8.5pt "Segoe UI", sans-serif; color: #7c8396; }
}
@page :first { @bottom-left { content: none; } @bottom-right { content: none; } }
@media print {
  :root { --ink: #1b1e27; --ink-2: #4a5163; --ink-3: #7c8396; --paper: #fff; --paper-2: #f6f7fa; --paper-3: #eef0f5;
    --line: #e2e5ec; --accent: #3d5afe; --accent-ink: #2a3fc4; --accent-soft: #eef1ff; --kw: #b4235a; }
  body { font-size: 10pt; line-height: 1.55; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
  .masthead { background: none; border-bottom: 0; }
  .masthead-inner { padding: 70mm 0 0; }
  .masthead-inner::before { content: ""; display: block; width: 56px; height: 5px; border-radius: 3px; background: var(--accent); margin-bottom: 22px; }
  h1 { font-size: 32pt; }
  .meta { flex-direction: column; gap: 10px; margin-top: 18mm; }
  .layout { display: block; padding: 0; max-width: none; }
  .toc { position: static; max-height: none; overflow: visible; padding: 0; break-before: page; break-after: page; font-size: 10pt; }
  .toc-title { font-size: 9pt; margin-bottom: 14px; }
  .toc ul { columns: 2; column-gap: 12mm; }
  .toc li { break-inside: avoid; }
  .toc li.l2 a { margin-top: 5px; }
  .toc li a { border-left: 0; padding: 1px 0; }
  .toc li.l3 a { padding-left: 18px; }
  article { max-width: none; padding: 0; }
  h2 { font-size: 17pt; margin-top: 26px; break-after: avoid; }
  h3 { font-size: 12.5pt; margin-top: 20px; break-after: avoid; }
  h2 + *, h3 + * { break-before: avoid; }
  .anchor { display: none; }
  hr { display: none; }
  a { text-decoration: none; color: inherit; }
  pre { white-space: pre-wrap; word-break: break-word; }
  pre code { font-size: 8.4pt; }
  table { font-size: 9pt; }
  tr { break-inside: avoid; }
  .table-wrap { overflow: visible; }
}
</style>
</head>
<body>
<header class="masthead"><div class="masthead-inner">
  <p class="eyebrow">Sight.Serialization · Specification</p>
  <h1>${title}</h1>
  <dl class="meta">${meta}</dl>
</div></header>
<div class="layout">
  <nav class="toc" aria-label="Contents"><p class="toc-title">Contents</p><ul>${tocHtml}</ul></nav>
  <article>
${article.replace(/<table>/g, '<div class="table-wrap"><table>').replace(/<\/table>/g, "</table></div>")}
  </article>
</div>
<script>
(() => {
  const links = new Map([...document.querySelectorAll(".toc a")].map((a) => [a.getAttribute("href").slice(1), a]));
  if (!("IntersectionObserver" in window)) return;
  let current;
  const io = new IntersectionObserver((entries) => {
    for (const e of entries) if (e.isIntersecting) {
      current?.classList.remove("active");
      current = links.get(e.target.id);
      current?.classList.add("active");
    }
  }, { rootMargin: "0px 0px -75% 0px" });
  document.querySelectorAll("article h2[id], article h3[id]").forEach((h) => io.observe(h));
})();
</script>
</body>
</html>
`;
  const htmlPath = path.replace(/\.md$/, ".html");
  writeFileSync(htmlPath, page);
  console.log("written", htmlPath);
  if (browser) printPdf(browser, htmlPath);
}
