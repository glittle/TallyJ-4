const fs = require('fs');
const path = require('path');

const dir = path.join(__dirname, 'dist', 'assets');
const files = fs.readdirSync(dir);
const mapFile = files.find(f => f.startsWith('admin-layout') && f.endsWith('.js.map'));

const mapContent = fs.readFileSync(path.join(dir, mapFile), 'utf8');
const map = JSON.parse(mapContent);

console.log(`Found ${map.sources.length} sources in ${mapFile}`);
console.log('First 5 sources:', map.sources.slice(0, 5));
console.log('Last 5 sources:', map.sources.slice(-5));

// What is the size of the generated map and JS file?
const jsFile = mapFile.replace('.map', '');
const jsStats = fs.statSync(path.join(dir, jsFile));
console.log(`\nJS file size: ${(jsStats.size / 1024).toFixed(2)} KB`);

// Print a small sample of the generated JS file
const jsCode = fs.readFileSync(path.join(dir, jsFile), 'utf8');
console.log('Start of JS file:');
console.log(jsCode.substring(0, 500));
