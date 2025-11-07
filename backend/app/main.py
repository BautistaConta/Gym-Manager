from fastapi import FastAPI
from app.db import init_db, close_db
from app.config import settings

app = FastAPI(title="Gym Manager API", version="1.0")

@app.on_event("startup")
async def startup_event():
    init_db()
    print("✅ Conexión a MongoDB inicializada.")

@app.on_event("shutdown")
async def shutdown_event():
    close_db()
    print("🛑 Conexión a MongoDB cerrada.")

@app.get("/ping")
async def ping():
    return {"message": "🏋️‍♂️ Gym API funcionando correctamente"}
