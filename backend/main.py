from fastapi import FastAPI

from api.v1.health import router as health_router

app = FastAPI(title="Company Brain API")
app.include_router(health_router)


@app.get("/")
async def root() -> dict[str, str]:
    return {"message": "Company Brain API"}
