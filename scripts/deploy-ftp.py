import os
import ftplib
import time
import sys

FTP_HOST = "ftp.irres.ir"
FTP_USER = "Faradid"
FTP_PASS = "%n91i13aH"
PUBLISH_DIR = r"F:\Projects\Faradid\publish"

def ensure_remote_dir(ftp, remote_path):
    parts = [p for p in remote_path.replace("\\", "/").split("/") if p]
    current = ""
    for part in parts:
        current += "/" + part
        try:
            ftp.mkd(current)
        except Exception:
            pass

def upload_directory(ftp, local_dir, remote_dir):
    ensure_remote_dir(ftp, remote_dir)

    protected_rel_paths = {
        "/App_Data/projects.json",
        "/App_Data/seo_alarms.json",
        "/App_Data/reports",
        "/App_Data/local_ranks",
        "/App_Data/crawls",
    }

    for item in os.listdir(local_dir):
        local_path = os.path.join(local_dir, item)
        remote_path = f"{remote_dir}/{item}".replace("//", "/")
        if remote_path in protected_rel_paths:
            print(f"Skipping runtime state: {remote_path}")
            continue
        
        if os.path.isdir(local_path):
            upload_directory(ftp, local_path, remote_path)
        else:
            print(f"Uploading: {remote_path}")
            try:
                with open(local_path, "rb") as f:
                    ftp.storbinary(f"STOR {remote_path}", f)
            except Exception as e:
                print(f"Error uploading {remote_path}: {e}")
                ensure_remote_dir(ftp, remote_dir)
                with open(local_path, "rb") as f:
                    ftp.storbinary(f"STOR {remote_path}", f)

def main():
    print(f"Connecting to FTP {FTP_HOST} as {FTP_USER}...")
    ftp = ftplib.FTP(FTP_HOST, timeout=30)
    ftp.login(FTP_USER, FTP_PASS)
    print("Logged in successfully. Remote pwd:", ftp.pwd())
    
    # 1. Put IIS in offline mode to release file locks
    print("[1/4] Uploading app_offline.htm to release IIS DLL file locks...")
    offline_content = b"<!DOCTYPE html><html><head><meta charset='utf-8'><title>Faradid Updating</title></head><body style='font-family:sans-serif;text-align:center;padding:50px;'><h2>\xd8\xaf\xd8\xb1 \xd8\xad\xd8\xa7\xd9\x84 \xd8\xa8\xd9\x87\xe2\x80\x8c\xd8\xb1\xd9\x88\xd8\xb2\xd8\xb1\xd8\xb3\xd8\xa7\xd9\x86\xdb\x8c...</h2></body></html>"
    from io import BytesIO
    ftp.storbinary("STOR /app_offline.htm", BytesIO(offline_content))
    
    print("[2/4] Waiting 3 seconds for IIS worker process to release handles...")
    time.sleep(3)
    
    # 2. Upload all published files
    print("[3/4] Uploading all application binaries, templates, and static assets...")
    upload_directory(ftp, PUBLISH_DIR, "")
    
    # 3. Bring application back online
    print("[4/4] Removing app_offline.htm to start IIS application...")
    try:
        ftp.delete("/app_offline.htm")
    except Exception as e:
        print(f"Notice when deleting app_offline.htm: {e}")

    ftp.quit()
    print("Deployment completed successfully!")

if __name__ == "__main__":
    main()
