# Gym Book admin (web)

The admin app's screens in the browser: dashboard, users (with every action, Gym Book Pro included), AI use and the
"Require Pro for AI" switch, and rank reports. Only admin accounts can sign in; the API checks every call.

A static Next.js export (one page; screens are picked by the part after `#`), served by nginx in Docker. It's its
own site, apart from the public website in `GymBook.Web`.

```bash
npm install
NEXT_PUBLIC_API_URL=https://your-api npm run dev
```

Deployed by the **Publish Admin Web** workflow. The API only answers browsers on the addresses in
`Cors__AllowedOrigins`, which it takes from the `ADMIN_WEB_URL` variable.
