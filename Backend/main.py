"""
Main FastAPI server for AI-Driven Semiconductor Digital Twin.
Module 9: Microservices Architecture & High-Throughput Telemetry Pipeline.
"""

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
import uvicorn
import asyncio
from typing import List
import json

from database import engine, Base
import models
from routers import sessions, wafers, telemetry, defects, recipes, socratic

# Initialize SQLite/PostgreSQL schema
Base.metadata.create_all(bind=engine)

app = FastAPI(
    title="Semiconductor Cleanroom Digital Twin Backend",
    description="High-throughput telemetry, wafer state serialization, physics rule engine integration, and Socratic AI microservices.",
    version="1.0.0"
)

# Enable CORS for local Unity VR client and web dashboards
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include API Routers under /api/v1
app.include_router(sessions.router, prefix="/api/v1")
app.include_router(wafers.router, prefix="/api/v1")
app.include_router(telemetry.router, prefix="/api/v1")
app.include_router(defects.router, prefix="/api/v1")
app.include_router(recipes.router, prefix="/api/v1")
app.include_router(socratic.router, prefix="/api/v1")


# --- WebSocket Real-Time Telemetry Hub ---
class ConnectionManager:
    def __init__(self):
        self.active_connections: List[WebSocket] = []

    async def connect(self, websocket: WebSocket):
        await websocket.accept()
        self.active_connections.append(websocket)

    def disconnect(self, websocket: WebSocket):
        if websocket in self.active_connections:
            self.active_connections.remove(websocket)

    async def broadcast(self, message: dict):
        dead_connections = []
        for connection in self.active_connections:
            try:
                await connection.send_json(message)
            except Exception:
                dead_connections.append(connection)
        for dc in dead_connections:
            self.disconnect(dc)

manager = ConnectionManager()

@app.websocket("/ws/telemetry")
async def websocket_telemetry_endpoint(websocket: WebSocket):
    """Real-time bi-directional telemetry and event stream for VR headsets and instructor panels."""
    await manager.connect(websocket)
    try:
        while True:
            data = await websocket.receive_text()
            try:
                payload = json.loads(data)
                # Broadcast incoming sensor event to connected observers
                await manager.broadcast({
                    "event": "telemetry_update",
                    "payload": payload
                })
            except json.JSONDecodeError:
                await websocket.send_text("Invalid JSON format")
    except WebSocketDisconnect:
        manager.disconnect(websocket)


@app.get("/health")
def health_check():
    """System health check endpoint for Unity TelemetryBridge connectivity probe."""
    return {
        "status": "healthy",
        "service": "SemiconductorDigitalTwin-Backend",
        "version": "1.0.0",
        "active_ws_clients": len(manager.active_connections)
    }

@app.get("/")
def root():
    return {
        "title": "Semiconductor Cleanroom VR Digital Twin API",
        "documentation": "/docs",
        "health": "/health",
        "api_v1": "/api/v1"
    }

if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8005, reload=True)
