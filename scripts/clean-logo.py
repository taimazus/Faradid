from PIL import Image, ImageOps
import numpy as np

src_path = "F:/Projects/Faradid/src/Faradid.Web/wwwroot/images/brand/faradid-logo.png"
img = Image.open(src_path).convert("RGBA")

# Bounding box is (184, 42, 456, 317)
cropped = img.crop((184, 42, 456, 317))

# Replace the white outside corners with transparent pixels
arr = np.array(cropped)
r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]

# Pixels that are near white (> 240 in all RGB channels) should be transparent
white_mask = (r > 235) & (g > 235) & (b > 235)
arr[white_mask, 3] = 0

clean_img = Image.fromarray(arr)

# Save as clean high-res transparent PNG
clean_img.save("F:/Projects/Faradid/src/Faradid.Web/wwwroot/images/brand/faradid-logo.png", "PNG")
clean_img.save("F:/Projects/Faradid/src/Faradid.Web/wwwroot/favicon.png", "PNG")

# Also save resized icons
icon_32 = clean_img.resize((32, 32), Image.Resampling.LANCZOS)
icon_32.save("F:/Projects/Faradid/src/Faradid.Web/wwwroot/favicon.ico")

print("Clean icon generated:", clean_img.size)
