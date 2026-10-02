"""
Builds GymBook/Resources/Raw/foods/usda_foods.tsv.gz, the food search's built-in foods, from USDA FoodData Central
(public domain): SR Legacy and the FNDDS survey foods. Download and unzip the two CSV datasets from
https://fdc.nal.usda.gov/download-datasets (FoodData_Central_sr_legacy_food_csv_2018-04.zip and
FoodData_Central_survey_food_csv_<date>.zip) into one folder, then:

    python3 tools/build_usda_foods.py <folder> GymBook/Resources/Raw/foods/usda_foods.tsv.gz

Each line: name, kcal, protein g, carbs g, fat g (all per 100 g), the first portion's grams and its description.
"""
import csv, gzip, glob, sys, os
out=[]
units={}
for d in sorted(glob.glob(os.path.join(sys.argv[1],'*/*/'))):
    for r in csv.DictReader(open(d+'measure_unit.csv',encoding='utf-8')): units[r['id']]=r['name']
    foods={r['fdc_id']:r['description'] for r in csv.DictReader(open(d+'food.csv',encoding='utf-8'))}
    nut={}
    want={'1003':'p','1004':'f','1005':'c','1008':'k','2047':'k2','2048':'k3','203':'p','204':'f','205':'c','208':'k'}
    for r in csv.DictReader(open(d+'food_nutrient.csv',encoding='utf-8')):
        k=want.get(r['nutrient_id'])
        if k and r['fdc_id'] in foods and r['amount']:
            nut.setdefault(r['fdc_id'],{})[k]=float(r['amount'])
    portion={}
    for r in csv.DictReader(open(d+'food_portion.csv',encoding='utf-8')):
        f=r['fdc_id']
        try: g=float(r['gram_weight'])
        except: continue
        if g<=0: continue
        seq=int(r['seq_num'] or 99)
        if f in portion and portion[f][0]<=seq: continue
        if r['portion_description'] and 'Quantity not specified' not in r['portion_description']:
            text=r['portion_description']
        else:
            u=units.get(r['measure_unit_id'],'')
            amt=r['amount']
            try: amt=('%g'%float(amt))
            except: amt=''
            name = r['modifier'] if u in ('undetermined','') else (u+(', '+r['modifier'] if r['modifier'] and not r['modifier'].isdigit() else ''))
            text=(amt+' '+name).strip()
        if not text or text[0].isdigit()==False and len(text)<2: text=''
        portion[f]=(seq,g,text)
    for f,name in foods.items():
        n=nut.get(f,{})
        k=n.get('k', n.get('k2', n.get('k3')))
        if k is None or 'p' not in n or 'c' not in n or 'f' not in n: continue
        name=name.replace('\t',' ').strip()
        g,text=(portion[f][1],portion[f][2]) if f in portion else ('','')
        out.append((name,k,n['p'],n['c'],n['f'],g,text))
# one per name
seen=set(); rows=[]
for r in out:
    key=r[0].lower()
    if key in seen: continue
    seen.add(key); rows.append(r)
fmt=lambda v: ('%g'%round(v,1)) if v!='' else ''
with gzip.open(sys.argv[2],'wt',encoding='utf-8',compresslevel=9) as w:
    for r in rows:
        w.write('\t'.join([r[0],fmt(r[1]),fmt(r[2]),fmt(r[3]),fmt(r[4]),fmt(r[5]) if r[5]!='' else '',r[6].replace('\t',' ')])+'\n')
print(len(rows))
