import os, re

log_path = r"C:\Users\ahmet\.gemini\antigravity\brain\ba339259-56e8-42a1-bf27-d25ae5f48eb3\.system_generated\logs\overview.txt"

with open(log_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Extract the block
start_sig = '1\t"C001"'
end_sig = '563\t"CC099"'

start_idx = text.rfind(start_sig)
if start_idx == -1:
    print("Could not find start signature.")
    exit(1)

end_idx = text.find('\n', text.find(end_sig, start_idx))
if end_idx == -1:
    end_idx = len(text)

block = text[start_idx:end_idx].strip()
lines = block.split('\n')

sql_path = r"d:\Projeler\YLCDORSE\YALCINDORSE\TumCariler_SQL_Sorgusu.sql"

values = []
for line in lines:
    line = line.strip()
    if not line:
        continue
        
    parts = line.split('\t')
    if len(parts) < 3:
        continue
        
    code = parts[1].strip('"')
    title = parts[2].strip('"').replace("'", "''")
    
    taxnum = parts[3].strip('"') if len(parts) > 3 and parts[3].strip('"') else ""
    taxoff = parts[4].strip('"') if len(parts) > 4 and parts[4].strip('"') else ""
    country = parts[5].strip('"') if len(parts) > 5 and parts[5].strip('"') else "TR"
    addr = parts[6].strip('"') if len(parts) > 6 and parts[6].strip('"') else ""
    rlimit = parts[7] if len(parts) > 7 and parts[7].strip() else "0"
    curr = parts[8] if len(parts) > 8 and parts[8].strip() else "0"
    
    taxnum_val = f"'{taxnum}'" if taxnum else "NULL"
    taxoff_val = f"'{taxoff}'" if taxoff else "NULL"
    addr_val = f"'{addr}'" if addr else "NULL"
    
    val = f"('{code}', '{title}', {taxnum_val}, {taxoff_val}, '{country}', {addr_val}, {rlimit}, {curr}, true)"
    values.append(val)

if values:
    with open(sql_path, "w", encoding="utf-8") as f:
        f.write('INSERT INTO "YLCustomers" \n')
        f.write('("CustomerCode", "Title", "TaxNumber", "TaxOffice", "Country", "Address", "RiskLimit", "CurrentBalance", "IsActive") \n')
        f.write('VALUES \n')
        f.write(",\n".join(values) + ";")
    print(f"Successfully wrote {len(values)} records to {sql_path}")
else:
    print("No records processed.")
