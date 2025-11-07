from fastapi import FastAPI
from app.db import init_db, close_db
from app.config import settings
from .routers import auth as auth_router 

app = FastAPI(title="Gym Manager API", version="1.0")

@app.on_event("startup")
async def startup_event():
    init_db()
    print("✅ Conexión a MongoDB inicializada.")

@app.on_event("shutdown")
async def shutdown_event():
    close_db()
    print("🛑 Conexión a MongoDB cerrada.")

app.include_router(auth_router.router)

@app.get("/ping")
async def ping():
    return {"message": "🏋️‍♂️ Gym API funcionando correctamente"}
