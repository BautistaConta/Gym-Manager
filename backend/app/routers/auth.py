# app/routers/auth.py
from fastapi import APIRouter, HTTPException, Depends, status
from fastapi.security import OAuth2PasswordRequestForm
from ..schemas import UserCreate, UserOut, Token
from ..auth import hash_password, verify_password, create_access_token, get_current_user
from ..crud import users as users_crud

router = APIRouter(prefix="/auth", tags=["auth"])

@router.post("/register", response_model=UserOut, status_code=status.HTTP_201_CREATED)
async def register(user_in: UserCreate):
    # 1) validar si email ya existe
    existing = await users_crud.get_user_by_email(user_in.email)
    if existing:
        raise HTTPException(status_code=400, detail="Email ya registrado")

    # 2) preparar documento
    user_doc = {
        "name": user_in.name,
        "email": user_in.email,
        "password_hash": hash_password(user_in.password),
        "role": user_in.role,
        "weight": user_in.weight,
        "height": user_in.height,
        "created_at": __import__("datetime").datetime.utcnow()
    }

    # 3) crear usuario en DB
    new_user = await users_crud.create_user(user_doc)
    # formatear salida sin password
    new_user["id"] = str(new_user["_id"])
    new_user.pop("_id", None)
    new_user.pop("password_hash", None)
    return new_user

@router.post("/login", response_model=Token)
async def login(form_data: OAuth2PasswordRequestForm = Depends()):
    user = await users_crud.get_user_by_email(form_data.username)
    if not user:
        raise HTTPException(status_code=400, detail="Credenciales incorrectas")
    if not verify_password(form_data.password, user["password_hash"]):
        raise HTTPException(status_code=400, detail="Credenciales incorrectas")

    access_token = create_access_token(subject=str(user["_id"]), extra={"role": user.get("role")})
    return {"access_token": access_token, "token_type": "bearer"}

@router.get("/me", response_model=UserOut)
async def me(current_user: dict = Depends(get_current_user)):
    # current_user ya viene sin password (en get_current_user lo limpiamos)
    # Aseguramos que el output tenga id string
    return {
        "id": current_user["id"],
        "name": current_user["name"],
        "email": current_user["email"],
        "role": current_user["role"],
        "weight": current_user.get("weight"),
        "height": current_user.get("height")
    }
