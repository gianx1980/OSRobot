# OS-Robot website

The public site at <https://www.os-robot.com>, built with [VitePress](https://vitepress.dev/).
The manual pages are generated from `docs/user-manual/`, so edit them there.

## Commands

```bash
npm install
npm run dev      # local site with live reload
npm run build    # writes the site to .vitepress/dist
npm test         # checks the last build, so run it after npm run build
```

## Publishing

The site is hosted at Aruba and published by FTP.

1. Check out `master`, because the published manual comes from the code you build.
2. Run `npm ci`, `npm run build` and `npm test`.
3. Upload the contents of `.vitepress/dist` to the web folder, overwriting existing files.
   Turn on "show hidden files" in your FTP client, or the `.htaccess` file won't be uploaded.

4. Run `npm run check-urls`, which checks the live site's pages and redirects. If Aruba's page
   cache shows old content, purge it and run the check again.

Page URLs must keep their trailing slash, so a new page goes in `name/index.md`, not `name.md`.
