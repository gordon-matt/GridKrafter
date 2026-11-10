import { fileURLToPath } from 'node:url'
import path from 'node:path'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..')

export const project = path.join(root, 'GridKrafter.CustomElements', 'GridKrafter.CustomElements.csproj')
export const publishDir = path.join(root, 'GridKrafter.CustomElements', 'bin', 'publish')
export const output = path.join(publishDir, 'wwwroot')
