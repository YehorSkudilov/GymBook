# GymBook.Web

The Gym Book marketing site: features, store links, privacy policy, terms, account deletion and support. A static
Next.js export (`out/`) served by nginx.

```bash
npm install
npm run dev        # http://localhost:3000
npm run build      # static site in out/
```

Store links come from `NEXT_PUBLIC_*` variables at build time (see `.env.example` and `src/lib/site.ts`); a platform
without one shows "Coming soon". Deployed by `.github/workflows/PublishWeb.yml`.
