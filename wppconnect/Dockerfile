FROM node:20-bullseye-slim

# Puppeteer/Chromium sistem bağımlılıkları
RUN apt-get update && apt-get install -y \
    git \
    chromium \
    ca-certificates \
    fonts-liberation \
    libgbm1 \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=true \
    PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

WORKDIR /app

# WPPConnect Server kaynak kodunu indir ve kur
RUN git clone --depth 1 https://github.com/wppconnect-team/wppconnect-server.git .
RUN npm install
RUN npm run build

EXPOSE 21465

CMD ["node", "dist/server.js"]
