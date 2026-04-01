/**
 * PostToolUse hook: auto-lint after Write/Edit on frontend files.
 * Catches lint errors immediately so Claude can fix them in the same turn.
 *
 * Scope: admin/** and saler/** TypeScript/TSX files only.
 */
import { readFileSync } from 'fs';
import { execSync } from 'child_process';
import { resolve } from 'path';

const input = JSON.parse(readFileSync('/dev/stdin', 'utf8'));
const filePath = input?.tool_input?.file_path || '';

// Only lint frontend TypeScript/React files
if (!filePath.match(/\.(tsx?|jsx?)$/)) {
  process.stdout.write(JSON.stringify({ continue: true }));
  process.exit(0);
}

const inAdmin = filePath.includes('admin/');
const inSaler = filePath.includes('saler/');
if (!inAdmin && !inSaler) {
  process.exit(0);
}

const projectDir = inSaler ? resolve('saler') : resolve('admin');
try {
  execSync(
    `npx eslint --no-warn-ignored --max-warnings 0 "${filePath}"`,
    { cwd: projectDir, timeout: 15000, stdio: ['pipe', 'pipe', 'pipe'] }
  );
  process.stdout.write(JSON.stringify({ continue: true }));
} catch (err) {
  const output = (err.stdout || err.stderr || '').toString();
  const filename = filePath.split('/').pop();
  process.stdout.write(JSON.stringify({
    continue: true,
    message: `ESLint in ${filename}:\n${output.slice(0, 600)}`
  }));
}
