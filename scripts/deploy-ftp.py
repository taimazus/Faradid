import os
import ftplib
import sys

FTP_HOST = "ftp.irres.ir"
FTP_USER = "Faradid"
FTP_PASS = "%n91i13aH"
PUBLISH_DIR = r"F:\Projects\Faradid\publish"

def upload_directory(ftp, local_dir, remote_dir):
    print(f"Deploying {local_dir} -> {remote_dir}...")
    
    # Try creating remote_dir if not root
    if remote_dir not in ["", "/", "."]:
        try:
            ftp.mkd(remote_dir)
        except Exception:
            pass  # Directory likely already exists

    for item in os.listdir(local_dir):
        local_path = os.path.join(local_dir, item)
        remote_path = f"{remote_dir}/{item}".replace("//", "/")
        
        if os.path.isdir(local_path):
            upload_directory(ftp, local_path, remote_path)
        else:
            print(f"Uploading: {remote_path}")
            try:
                with open(local_path, "rb") as f:
                    ftp.storbinary(f"STOR {remote_path}", f)
            except Exception as e:
                print(f"Error uploading {remote_path}: {e}")
                raise e

def main():
    print(f"Connecting to FTP {FTP_HOST} as {FTP_USER}...")
    ftp = ftplib.FTP(FTP_HOST, timeout=30)
    ftp.login(FTP_USER, FTP_PASS)
    print("Logged in successfully. Remote pwd:", ftp.pwd())
    
    upload_directory(ftp, PUBLISH_DIR, "")
    
    ftp.quit()
    print("Deployment completed successfully!")

if __name__ == "__main__":
    main()
