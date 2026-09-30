# GymBook.Admin

The Gym Book admin site: users, what they train, AI usage, and account actions (disable, sign out everywhere, reset AI
quota, password reset email, verify email; SuperAdmins also change roles and delete accounts). Styled like the app.

Admins sign in with their normal Gym Book account; only accounts with the `Admin` or `SuperAdmin` role get in (see
`GymBook.Api/Admin/AdminRoles.cs`). The first SuperAdmin comes from the API's `Admin__BootstrapEmails`.

```bash
cp .env.example .env.local   # set API_BASE_URL and SESSION_SECRET
npm install
npm run dev                  # http://localhost:3000
```

The browser only talks to this app: `src/app/api/session` signs in and out, and `src/app/api/admin/[...path]` forwards
to the API's `/api/admin/*` with the admin's token, which is kept in an encrypted httpOnly cookie. Deployed by
`.github/workflows/PublishAdmin.yml`.
