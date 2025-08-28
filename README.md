# Trading-website
A simple **SPA** built with **Nuxt + Vue 3 (frontend)** and **.NET 9 (backend)** that visualizes a real-time **order book chart** (bids in green, asks in red). Data is streamed via **WebSockets** and displayed with **D3.js**.

---

## ✨ Features
- 📊 Interactive bar chart with bids/asks  
- 🎚️ Adjustable number of entries displayed  
- 📈 Price range dynamically updates when entries change  
- 🔄 Live updates via WebSocket  
- 🖱️ Tooltips on hover for quick info  
- ⚡ Fast and lightweight SPA setup  
- 🖥️ Backend in .NET with WebSocket broadcasting  

### Order book chart example:
<img width="2559" height="1196" alt="Screenshot 2025-08-27 191213" src="https://github.com/user-attachments/assets/547d2ec2-c148-44f6-9c72-99e24798cbbd" />

### Audit log example:
<img width="2559" height="1221" alt="Screenshot 2025-08-27 191233" src="https://github.com/user-attachments/assets/05d1760d-2231-431c-94dd-6679e1599328" />

---

## 🛠️ Tech Stack
- **Frontend:** Nuxt + Vue 3 + D3.js  
- **Backend:** ASP.NET Core (.NET 9) + WebSockets  

## ⚙️ Installation

### 1. Clone the Repository
```sh
git clone git@github.com:mshchygol/trading-website.git
cd trading-website
```

### 2. Backend Setup (.NET)
```sh
cd backend
dotnet restore
dotnet run
```

### 3. Frontend Setup (Nuxt + Vue 3)
```sh
cd application
npm install
npm run dev
```
