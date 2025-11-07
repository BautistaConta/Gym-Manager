from pydantic import BaseSettings, AnyUrl

class Settings(BaseSettings):
    MONGODB_URI: AnyUrl
    MONGODB_DB: str = "gymdb"
    SECRET_KEY: str
    ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 60

    class Config:
        env_file = ".env"

settings = Settings()
