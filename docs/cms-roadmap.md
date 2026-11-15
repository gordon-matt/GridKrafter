# GridKrafter Platform: CMS and sales roadmap

Goal: turn the Wisej demo into an enterprise platform in the spirit of [Oqtane](https://github.com/oqtane/oqtane.framework):
several sites in one installation, pages built from modules, modules installed while it runs, a public website, and a
sales (CRM) module, all managed from the Wisej back office.

## Decisions (agreed)

| Topic | Decision |
|---|---|
| Audience | **Public website** rendered by the API as plain, indexable HTML; **everything is managed in the Wisej app** (back office). |
| Tenancy | **Several sites from the start.** A site is found by its host name (alias). Every record carries a site id. |
| Database | **SQLite** with the current hand-written data access. Each module owns its tables and its versioned schema scripts. |
| Modules | **Installed while the platform runs** from packages, as Oqtane does. Built-in modules (Content, Sales, Files) use the same contract. |
| Data access | Only the API opens the database (unchanged). The Wisej app and the public site go through it. |

## Architecture

```
GridKrafter.Platform.Abstractions   module contract shared by the API, the Wisej app and module packages
GridKrafter.Demo.Api                 platform server: sites, pages, permissions, module runtime, public site, data
GridKrafter.WisejDemo                back office: sites, pages, content, files, users, sales, extensions
GridKrafter.Demo.AppHost             Aspire: starts both
Modules/<Name>/                      built-in modules, packaged the same way as third-party ones
```

### Concepts (as in Oqtane)

- **Site**: a website with its aliases (host names), its theme, its pages, users and roles.
- **Page**: a node of the site's page tree (path, title, SEO fields, permissions, visibility, theme override).
- **Module definition**: an installed kind of module (Content, Sales pipeline, Contact form...), with its version.
- **Module instance**: a module placed on a page, in a pane, with its own settings and permissions.
- **Permissions**: View and Edit per role on sites, pages, module instances and folders; Host (whole install)
  and Site administrator roles above them.
- **Layout**: a page's panes and module positions are a GridKrafter board, so the back office edits page layouts with
  the existing `GridBoard` and the public site renders the same grid.

### Module contract

A module package (`.gkmodule`, a zip) holds a manifest (id, name, version, platform version range, publisher,
signature), a **server** assembly (endpoints, schema scripts, public rendering) and a **back-office** assembly
(Wisej editors and settings). The API loads each server assembly into its own collectible load context; the Wisej app
loads the back-office assembly at start-up. Installing or upgrading needs a restart of the Wisej app; uninstalling
removes the module's instances and, when asked, its tables.

Security: only the Host role installs modules; packages must be signed by a publisher on the installation's allow
list; every install, upgrade and removal is audited.

## Phases

1. **Platform foundation**: sites and aliases; users with per-site roles; page tree and page permissions;
   the module contract project; the module runtime (load, register, schema scripts per module) used by a first
   built-in module; back office: site switcher, Sites, Pages, per-site Users & roles.
2. **Content**: Content (HTML) module with drafts, versions and publish; page layout editor on `GridBoard`; files and
   media library; public site rendering with themes, friendly URLs, meta tags, sitemap and robots; output caching.
3. **Sales (CRM)**: accounts, contacts, leads, opportunities with a pipeline board, activities and tasks, products
   and price lists, quotes to orders, sales dashboard widgets on the existing boards.
4. **Extensions**: package format, upload, install, upgrade, uninstall while running, signing and allow list, theme
   packages for the public site.
5. **Enterprise operations**: scheduled jobs, notifications and e-mail, localization, search, backups, health checks,
   rate limits, single sign-on.
