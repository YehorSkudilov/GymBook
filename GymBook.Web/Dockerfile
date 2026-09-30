# The marketing site: a static Next.js export served by nginx.
#   docker build -t gymbook-web GymBook.Web
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Store links are baked into the pages at build time (see src/lib/site.ts).
ARG NEXT_PUBLIC_SITE_URL
ARG NEXT_PUBLIC_APP_STORE_URL
ARG NEXT_PUBLIC_PLAY_STORE_URL
ARG NEXT_PUBLIC_MS_STORE_URL
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/out /usr/share/nginx/html
EXPOSE 80
