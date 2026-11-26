from fastapi import FastAPI

app = FastAPI(title="Company Brain API")


@app.get("/")
async def root() -> dict[str, str]:
    return {"message": "Company Brain API"}
