const fs = require("fs");
const path = require("path");
const dir = "dist/assets";
const files = fs.readdirSync(dir)
  .filter(f => /voting|locale-|index-/.test(f) && f.endsWith(".js"))
  .sort();
files.forEach(f => {
  const p = path.join(dir, f);
  const size = fs.statSync(p).size;
  console.log(f + ": " + size + " bytes");
});
