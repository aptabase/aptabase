# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "aptabase==0.1.0",
#     "python-dotenv==1.2.1",
# ]
# ///

import asyncio
import os

from aptabase import Aptabase
from dotenv import load_dotenv

load_dotenv()


async def main() -> None:
    app_key = os.getenv("APTABASE_APP_KEY")
    base_url = os.getenv("APTABASE_BASE_URL")
    print(f"Config: app_key={app_key}, base_url={base_url}")

    async with Aptabase(app_key, base_url=base_url) as client:
        await client.track(
            "test_event",
            {
                "source": "self_host_test",
                "ok": True,
            },
        )
        await client.flush()


if __name__ == "__main__":
    asyncio.run(main())
