FROM node:20-alpine AS build
WORKDIR /app

ARG DISCORD_CLIENT_ID
ARG DISCORD_REDIRECT_URI
ARG BACKEND_HOST

ENV DISCORD_CLIENT_ID=${DISCORD_CLIENT_ID}
ENV DISCORD_REDIRECT_URI=${DISCORD_REDIRECT_URI}
ENV BACKEND_HOST=${BACKEND_HOST}

COPY package.json pnpm-lock.yaml ./
RUN corepack enable && corepack prepare pnpm@9.12.1 --activate && pnpm install --frozen-lockfile

COPY . .
RUN pnpm build

FROM node:20-alpine
WORKDIR /app
ENV NODE_ENV=production

COPY --from=build /app/package.json /app/pnpm-lock.yaml ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/build ./build

EXPOSE 3000
CMD ["node", "build"]
