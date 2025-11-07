# app/crud/users.py
from bson import ObjectId
from ..db import db

async def get_user_by_email(email: str):
    return await db["users"].find_one({"email": email})

async def get_user_by_id(user_id: str):
    return await db["users"].find_one({"_id": ObjectId(user_id)})

async def create_user(user_doc: dict):
    res = await db["users"].insert_one(user_doc)
    return await get_user_by_id(str(res.inserted_id))
