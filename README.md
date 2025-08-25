# Trading-website
A simple **SPA** built with **Vue 3 (frontend)** and **.NET 7 (backend)** that visualizes a real-time **order book chart** (bids in green, asks in red). Data is streamed via **WebSockets** and displayed with **D3.js**.

---

## ✨ Features
- 📊 Interactive bar chart with bids/asks  
- 🔄 Live updates via WebSocket  
- 🖱️ Tooltips on hover for quick info  
- ⚡ Fast and lightweight SPA setup  
- 🖥️ Backend in .NET with WebSocket broadcasting  

---

## 🛠️ Tech Stack
- **Frontend:** Vue 3 + Vite + D3.js  
- **Backend:** ASP.NET Core (.NET 7) + WebSockets  

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

### 3. Frontend Setup (Vue 3 + Vite)
```sh
cd frontend
npm install
npm run dev
```