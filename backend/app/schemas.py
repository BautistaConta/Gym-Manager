# app/schemas.py
from pydantic import BaseModel, EmailStr, Field
from typing import Optional

class UserCreate(BaseModel):
    name: str = Field(..., min_length=1)
    email: EmailStr
    password: str = Field(..., min_length=6)
    role: str = Field(..., regex="^(alumno|profesor|gestor)$")
    weight: Optional[float]
    height: Optional[float]

class UserOut(BaseModel):
    id: str
    name: str
    email: EmailStr
    role: str
    weight: Optional[float]
    height: Optional[float]

class Token(BaseModel):
    access_token: str
    token_type: str = "bearer"
