// Publishes GridKrafter.CustomElements, whose output vite.config.js serves at /_framework and /_content.
import { execFileSync } from 'node:child_process'
import { existsSync } from 'node:fs'
import { output, project, publishDir } from './paths.mjs'

const force = process.argv.includes('--force')

if (!force && existsSync(output)) {
  console.log('GridKrafter custom element already published. Run "npm run gridkrafter" to republish.')
  process.exit(0)
}

console.log('Publishing the GridKrafter custom element...')
execFileSync('dotnet', ['publish', project, '-c', 'Release', '-o', publishDir, '--nologo'], { stdio: 'inherit' })
